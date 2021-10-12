package util

import (
	"net/url"
)

func getListenerPort(uri *url.URL) string {
	s := uri.Port()
	if len(s) == 0 {
		s = "2375"
	}
	return ":" + s
}
