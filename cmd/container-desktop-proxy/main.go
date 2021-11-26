package main

import (
	"bytes"
	"crypto/tls"
	"crypto/x509"
	"io"
	"io/ioutil"
	"net/http"
	"net/url"
	"strconv"
	"strings"

	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"

	"github.com/container-desktop/container-desktop/internal/httputil"
	"github.com/container-desktop/container-desktop/internal/rewrite"
	"github.com/container-desktop/container-desktop/internal/util"

	"github.com/spf13/cobra"
)

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
			body, err := rewrite.RewriteBody(r.Body, r.URL.Path, flags.wslDistroName, logger)
			if len(body) > 0 && err == nil {
				logger.Debug("Request body was rewritten")
				r.Body = io.NopCloser(bytes.NewReader(body))
				r.ContentLength = int64(len(body))
			}
		}
		proxy.ModifyResponse = func(r *http.Response) error {
			logger.Debugf("Original response content-length: %d", r.ContentLength)
			body, err := rewrite.RewriteBody(r.Body, r.Request.URL.Path, flags.wslDistroName, logger)
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
