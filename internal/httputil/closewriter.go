package httputil

type CloseWriter interface {
	CloseWrite() error
}

type CloseReader interface {
	CloseRead() error
}
