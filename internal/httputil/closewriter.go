package httputil

type CloseWriter interface {
	CloseWrite() error
}
