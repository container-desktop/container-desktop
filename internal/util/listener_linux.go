//go:build linux
// +build linux

package util

import (
	"fmt"
	"net"
	"net/url"
)

func CreateListener(uri *url.URL) (net.Listener, error) {
	switch uri.Scheme {
	case "http":
		return net.Listen("tcp", getListenerPort(uri))
	case "unix":
		return net.Listen("unix", uri.Path)
	default:
		return nil, fmt.Errorf("the scheme %q is not supported", uri.Scheme)
	}
}
