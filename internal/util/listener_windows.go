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
		c := winio.PipeConfig{
			//SecurityDescriptor: nil,
			MessageMode:      true,  // Use message mode so that CloseWrite() is supported
			InputBufferSize:  65536, // Use 64KB buffers to improve performance
			OutputBufferSize: 65536,
		}
		return winio.ListenPipe(uri.Path, &c)
	case "http":
		return net.Listen("tcp", getListenerPort(uri))
	default:
		return nil, fmt.Errorf("the scheme %q is not supported", uri.Scheme)
	}
}
