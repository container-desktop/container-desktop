//go:build windows
// +build windows

package util

import (
	"fmt"
	"net"
	"net/url"

	"github.com/Microsoft/go-winio"
)

func CreateListener(uri *url.URL) (net.Listener, error) {
	switch uri.Scheme {
	case "npipe":
		return winio.ListenPipe(uri.Path, nil)
	case "http":
		return net.Listen("tcp", getListenerPort(uri))
	default:
		return nil, fmt.Errorf("the scheme %q is not supported", uri.Scheme)
	}
}
