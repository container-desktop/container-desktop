package main

import (
	"bytes"
	"crypto/tls"
	"crypto/x509"
	"encoding/json"
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"regexp"
	rt "runtime"
	"strings"

	"github.com/container-desktop/container-desktop/internal/util"

	"github.com/spf13/cobra"
)

// GET /containers/json
// POST /containers/create
// GET /containers/{id}/json

type RootFlags struct {
	version       bool
	listenAddress string
	targetAddress string
	wslDistroName string
	tlsKey        string
	tlsCert       string
	tlsCa         string
}

var flags = RootFlags{}

var rootCmd = &cobra.Command{
	Use:   "container-desktop-proxy",
	Short: "resolves and rewrites path binds.",
	Run: func(cmd *cobra.Command, args []string) {
		listenUri := parseUri(flags.listenAddress)
		targetUri := parseUri(flags.targetAddress)
		fmt.Printf("listen uri: %s, target uri: %s\n", listenUri, targetUri)

		cert, err := tls.LoadX509KeyPair(flags.tlsCert, flags.tlsKey)
		if err != nil {
			log.Fatalln(err)
		}

		caCert, err := ioutil.ReadFile(flags.tlsCa)
		if err != nil {
			log.Fatalln(err)
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
			log.Fatalln(err)
		}
		proxy := httputil.NewSingleHostReverseProxy(targetUri)
		proxy.Transport = transport
		orgDirector := proxy.Director
		proxy.Director = func(r *http.Request) {
			orgDirector(r)
			body, err := rewriteBody(r.Body, r.URL.Path, `(/.*?)?/containers/create`, rewriteBinds)
			if len(body) > 0 && err == nil {
				r.Body = io.NopCloser(bytes.NewReader(body))
				r.ContentLength = int64(len(body))
			}
		}
		proxy.ModifyResponse = func(r *http.Response) error {

			body, err := rewriteBody(r.Body, r.Request.URL.Path, `(/.*?)?/containers(/.*?)?/json`, rewriteMounts)
			if err != nil {
				return err
			}
			if len(body) > 0 {
				r.Body = io.NopCloser(bytes.NewReader(body))
				r.ContentLength = int64(len(body))
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

func rewriteBody(body io.ReadCloser, urlPath string, pathRegexp string, rewriter func(map[string]interface{}, string)) (rewrittenBody []byte, err error) {
	if body != nil {
		ok, err := regexp.MatchString(pathRegexp, urlPath)
		if err != nil {
			return nil, err
		}
		if ok {
			buf, err := io.ReadAll(body)
			if err != nil {
				return nil, err
			}
			var jsonArray []interface{}
			isArray := false
			if buf[0] == '{' {
				jsonMap := make(map[string]interface{})
				err = json.Unmarshal(buf, &jsonMap)
				if err != nil {
					return nil, err
				}
				jsonArray = make([]interface{}, 1)
				jsonArray[0] = jsonMap
			} else if buf[0] == '[' {
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
				return buf, nil
			}
		}
	}
	return nil, nil
}

func rewriteBinds(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["HostConfig"]
	if ok {
		hostConfig, ok := o.(map[string]interface{})
		if ok {
			o, ok := hostConfig["Binds"]
			if ok {
				binds, ok := o.([]interface{})
				if ok {
					for i, bind := range binds {
						s := bind.(string)
						s = strings.Replace(s, "\\", "/", -1)
						parts := strings.Split(s, ":")
						if rt.GOOS == "windows" {
							if parts[0] != "/" && len(parts[0]) == 1 {
								s = path + "/" + strings.ToLower(parts[0])
								s += strings.Join(parts[1:], ":")
							}
						} else if strings.HasPrefix(s, "/") {
							s = path + s
						}
						binds[i] = s
					}
				}
			}
		}
	}
}

func rewriteMounts(jsonMap map[string]interface{}, path string) {
	o, ok := jsonMap["Mounts"]
	if ok {
		mounts, ok := o.([]interface{})
		if ok {
			for _, o := range mounts {
				mount, ok := o.(map[string]interface{})
				if ok {
					t := mount["Type"].(string)
					if t == "bind" {
						s := mount["Source"].(string)
						if strings.HasPrefix(s, "/mnt/host/") {
							s = s[10:]
							parts := strings.Split(s, "/")
							s = parts[0] + ":/" + strings.Join(parts[1:], "/")
							s = strings.Replace(s, "/", "\\", -1)
						} else if strings.HasPrefix(s, "/mnt/wsl/") {
							parts := strings.Split(s[9:], "/")
							s = strings.Join(parts[1:], "/")
						}
						mount["Source"] = s
					}
				}
			}
		}
	}
	o, ok = jsonMap["HostConfig"]
	if ok {
		hostConfig, ok := o.(map[string]interface{})
		if ok {
			o, ok := hostConfig["Binds"]
			if ok {
				binds, ok := o.([]interface{})
				if ok {
					for i, bind := range binds {
						s := bind.(string)
						parts := strings.Split(s, ":")
						if strings.HasPrefix(parts[0], "/mnt/host/") {
							p := parts[0][10:]
							parts2 := strings.Split(p, "/")
							p = parts2[0] + ":/" + strings.Join(parts2[1:], "/")
							parts[0] = strings.Replace(p, "/", "\\", -1)
						} else if strings.HasPrefix(parts[0], "/mnt/wsl/") {
							parts2 := strings.Split(parts[0][9:], "/")
							parts[0] = strings.Join(parts2[1:], "/")
						}
						binds[i] = strings.Join(parts, ":")
					}
				}
			}
		}
	}
}

func parseUri(s string) *url.URL {
	uri, err := url.Parse(s)
	if err != nil {
		log.Fatalf("%q is not a valid uri.", s)
	}
	return uri
}

func main() {
	if err := rootCmd.Execute(); err != nil {
		log.Fatalln(err)
	}
}

func init() {
	cobra.OnInitialize(initConfig)
	rootCmd.Flags().StringVarP(&flags.listenAddress, "listen-address", "l", "", "The listener address.")
	rootCmd.MarkFlagRequired("listen-address")

	rootCmd.Flags().StringVarP(&flags.targetAddress, "target-address", "t", "", "The target address.")
	rootCmd.MarkPersistentFlagRequired("target-address")

	rootCmd.Flags().StringVarP(&flags.wslDistroName, "wsl-distro-name", "d", "", "The WSL distro name.")

	rootCmd.Flags().StringVarP(&flags.tlsKey, "tls-key", "", "", "The TLS client private key")
	rootCmd.Flags().StringVarP(&flags.tlsCert, "tls-cert", "", "", "The TLS client certificate")
	rootCmd.Flags().StringVarP(&flags.tlsCa, "tls-ca", "", "", "The TLS CA certificate")
}

func initConfig() {

}
