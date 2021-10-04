package main

import (
	"bytes"
	"crypto/tls"
	"crypto/x509"
	"encoding/json"
	"io"
	"io/ioutil"
	"net/http"
	"net/http/httputil"
	"net/url"
	"regexp"
	rt "runtime"
	"strconv"
	"strings"

	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"

	"github.com/container-desktop/container-desktop/internal/util"

	"github.com/spf13/cobra"
)

// GET /containers/json
// POST /containers/create
// GET /containers/{id}/json

// GET /services
// POST /services/create
// GET /services/{id}
// POST /services/{id}/update
// GET /tasks
// GET /tasks/{id}

type RewriteMapItem struct {
	method   string
	pattern  string
	rewriter func(map[string]interface{}, string)
}

var rewriteMappings = []RewriteMapItem{
	{"GET", `(/.*?)?/containers(/.*?)?/json`, rewriteContainerSummary},
	{"POST", `(/.*?)?/containers/create`, rewriteContainerConfig},
	{"POST", `(/.*?)?/services/create`, rewriteServiceSpec},
	{"POST", `(/.*?)?/services/(/.*?)/update`, rewriteServiceSpec},
	{"GET", `(/.*?)?/services(/.*?)$`, rewriteService},
	{"GET", `(/.*?)?/tasks(/.*?)?`, rewriteTask},
}

type RootFlags struct {
	listenAddress string
	targetAddress string
	wslDistroName string
	tlsKey        string
	tlsCert       string
	tlsCa         string
	logLevel      string
}

var flags = RootFlags{}
var logger *zap.SugaredLogger
var baseLogger *zap.Logger
var loggerConfig zap.Config

var rootCmd = &cobra.Command{
	Use:   "container-desktop-proxy",
	Short: "resolves and rewrites path binds.",
	Run: func(cmd *cobra.Command, args []string) {
		listenUri := parseUri(flags.listenAddress)
		targetUri := parseUri(flags.targetAddress)
		logLevel := parseLogLevel(flags.logLevel)
		loggerConfig.Level.SetLevel(logLevel)

		logger.Infof("listen uri: %s, target uri: %s\n", listenUri, targetUri)

		cert, err := tls.LoadX509KeyPair(flags.tlsCert, flags.tlsKey)
		if err != nil {
			logger.Fatal(err)
		}

		caCert, err := ioutil.ReadFile(flags.tlsCa)
		if err != nil {
			logger.Fatal(err)
		}

		caCertPool := x509.NewCertPool()
		caCertPool.AppendCertsFromPEM(caCert)

		tlsConfig := &tls.Config{
			Certificates: []tls.Certificate{cert},
			RootCAs:      caCertPool,
		}
		transport := &http.Transport{TLSClientConfig: tlsConfig}

		listener, err := util.CreateListener(listenUri)
		if err != nil {
			logger.Fatal(err)
		}
		proxy := httputil.NewSingleHostReverseProxy(targetUri)
		proxy.Transport = transport
		orgDirector := proxy.Director
		proxy.Director = func(r *http.Request) {
			orgDirector(r)
			logger.Debugf("Requesting url: %s: %s", r.Method, r.URL)
			body, err := rewriteBody(r.Body, r.URL.Path)
			if len(body) > 0 && err == nil {
				logger.Debug("Request body was rewritten")
				r.Body = io.NopCloser(bytes.NewReader(body))
				r.ContentLength = int64(len(body))
			}
		}
		proxy.ModifyResponse = func(r *http.Response) error {
			logger.Debugf("Original response content-length: %d", r.ContentLength)
			body, err := rewriteBody(r.Body, r.Request.URL.Path)
			if err != nil {
				return err
			}
			if len(body) > 0 {
				logger.Debug("Response body was rewritten.")
				r.Body = io.NopCloser(bytes.NewReader(body))
				r.ContentLength = int64(len(body))
				r.Header.Set("Content-Length", strconv.FormatInt(r.ContentLength, 10))
			}
			return nil
		}

		http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
			r.Host = targetUri.Host
			proxy.ServeHTTP(w, r)
		})

		http.Serve(listener, nil)
	},
}

func getRewriter(urlPath string) (func(map[string]interface{}, string), bool) {
	for _, item := range rewriteMappings {
		ok, err := regexp.MatchString(item.pattern, urlPath)
		if err == nil && ok {
			return item.rewriter, true
		}
	}
	return nil, false
}

func rewriteBody(body io.ReadCloser, urlPath string) (rewrittenBody []byte, err error) {
	if body != nil {
		rewriter, ok := getRewriter(urlPath)
		if ok {
			buf, err := io.ReadAll(body)
			if baseLogger.Core().Enabled(zapcore.DebugLevel) {
				logger.Debugf("Original body: %s", string(buf))
			}
			if err != nil {
				return nil, err
			}
			var jsonArray []interface{}
			isArray := false
			if buf[0] == '{' {
				logger.Debug("Body is a JSON object")
				jsonMap := make(map[string]interface{})
				err = json.Unmarshal(buf, &jsonMap)
				if err != nil {
					return nil, err
				}
				jsonArray = make([]interface{}, 1)
				jsonArray[0] = jsonMap
			} else if buf[0] == '[' {
				logger.Debug("Body is a JSON array")
				isArray = true
				err := json.Unmarshal(buf, &jsonArray)
				if err != nil {
					return nil, err
				}
			}
			if jsonArray != nil {
				path := "/mnt/"
				if len(flags.wslDistroName) > 0 {
					path += "wsl/" + flags.wslDistroName
				} else {
					path += "host"
				}
				logger.Debugf("Rewrite with base path: %s", path)
				for _, item := range jsonArray {
					m, ok := item.(map[string]interface{})
					if ok {
						rewriter(m, path)
					}
				}
				if isArray {
					buf, err = json.Marshal(jsonArray)
				} else {
					buf, err = json.Marshal(jsonArray[0])
				}
				if err != nil {
					return nil, err
				}

				if baseLogger.Core().Enabled(zapcore.DebugLevel) {
					logger.Debugf("Rewritten body: %s", string(buf))
				}
				return buf, nil
			}
		}
	}
	return nil, nil
}

func rewriteContainerSummary(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["HostConfig"]
	if ok {
		hostConfig, ok := o.(map[string]interface{})
		if ok {
			rewriteHostConfig(hostConfig, path)
		}
	}
	o, ok = jsonMap["Mounts"]
	if ok {
		mounts, ok := o.([]interface{})
		if ok {
			rewriteMounts(mounts, path)
		}
	}
}

func rewriteContainerConfig(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["HostConfig"]
	if ok {
		hostConfig, ok := o.(map[string]interface{})
		if ok {
			rewriteHostConfig(hostConfig, path)
		}
	}
}

func rewriteService(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["Spec"]
	if ok {
		spec, ok := o.(map[string]interface{})
		if ok {
			rewriteServiceSpec(spec, path)
		}
	}
}

func rewriteServiceSpec(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["TaskTemplate"]
	if ok {
		taskSpec, ok := o.(map[string]interface{})
		if ok {
			rewriteTaskSpec(taskSpec, path)
		}
	}
}

func rewriteTaskSpec(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["ContainerSpec"]
	if ok {
		containerSpec, ok := o.(map[string]interface{})
		if ok {
			rewriteContainerSpec(containerSpec, path)
		}
	}
}

func rewriteContainerSpec(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["Mounts"]
	if ok {
		mounts, ok := o.([]interface{})
		if ok {
			rewriteMounts(mounts, path)
		}
	}
}

func rewriteTask(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["Spec"]
	if ok {
		taskSpec, ok := o.(map[string]interface{})
		if ok {
			rewriteTaskSpec(taskSpec, path)
		}
	}
}

func rewriteHostConfig(hostConfig map[string]interface{}, path string) {
	o, ok := hostConfig["Binds"]
	if ok {
		binds, ok := o.([]interface{})
		if ok {
			for i, bind := range binds {
				s := bind.(string)
				s = mapPath(s, path)
				binds[i] = s
			}
		}
	}
}

func rewriteMounts(mounts []interface{}, path string) {
	for _, o := range mounts {
		mount, ok := o.(map[string]interface{})
		if ok {
			t := mount["Type"].(string)
			if t == "bind" {
				s := mount["Source"].(string)
				s = mapPath(s, path)
				mount["Source"] = s
			}
		}
	}
}

func mapPath(s string, path string) string {
	s = strings.Replace(s, "\\", "/", -1)
	parts := strings.Split(s, ":")
	if strings.HasPrefix(parts[0], "/mnt/host/") {
		p := parts[0][10:]
		parts2 := strings.Split(p, "/")
		p = parts2[0] + ":/" + strings.Join(parts2[1:], "/")
		parts[0] = strings.Replace(p, "/", "\\", -1)
		s = strings.Join(parts, ":")
	} else if strings.HasPrefix(parts[0], "/mnt/wsl/") {
		parts2 := strings.Split(parts[0][9:], "/")
		parts[0] = strings.Join(parts2[1:], "/")
		s = strings.Join(parts, ":")
	} else if rt.GOOS == "windows" {
		if parts[0] != "/" && len(parts[0]) == 1 {
			s = path + "/" + strings.ToLower(parts[0])
			s += strings.Join(parts[1:], ":")
		}
	} else if strings.HasPrefix(s, "/") {
		s = path + s
	}
	return s
}

func parseUri(s string) *url.URL {
	uri, err := url.Parse(s)
	if err != nil {
		logger.Fatalf("%q is not a valid uri.", s)
	}
	return uri
}

func parseLogLevel(s string) zapcore.Level {
	switch strings.ToLower(s) {
	case "panic":
		return zapcore.PanicLevel
	case "fatal":
		return zapcore.FatalLevel
	case "error":
		return zapcore.ErrorLevel
	case "warn":
		return zapcore.WarnLevel
	case "info":
		return zapcore.InfoLevel
	case "debug":
		return zapcore.DebugLevel
	default:
		return zapcore.InfoLevel
	}
}

func main() {
	loggerConfig = zap.NewProductionConfig()
	loggerConfig.OutputPaths[0] = "stdout"
	baseLogger, _ = loggerConfig.Build()
	logger = baseLogger.Sugar()
	defer logger.Sync()
	if err := rootCmd.Execute(); err != nil {
		logger.Fatal(err)
	}
}

func init() {
	cobra.OnInitialize(initConfig)
	rootCmd.Flags().StringVarP(&flags.listenAddress, "listen-address", "l", "", "The listener address")
	rootCmd.MarkFlagRequired("listen-address")

	rootCmd.Flags().StringVarP(&flags.targetAddress, "target-address", "t", "", "The target address")
	rootCmd.MarkPersistentFlagRequired("target-address")

	rootCmd.Flags().StringVarP(&flags.wslDistroName, "wsl-distro-name", "d", "", "The WSL distro name")

	rootCmd.Flags().StringVarP(&flags.tlsKey, "tls-key", "", "", "The TLS client private key")
	rootCmd.Flags().StringVarP(&flags.tlsCert, "tls-cert", "", "", "The TLS client certificate")
	rootCmd.Flags().StringVarP(&flags.tlsCa, "tls-ca", "", "", "The TLS CA certificate")
	rootCmd.Flags().StringVarP(&flags.logLevel, "log-level", "", "Info", "Specifies the log level. If omitted will default to Info")
}

func initConfig() {

}
