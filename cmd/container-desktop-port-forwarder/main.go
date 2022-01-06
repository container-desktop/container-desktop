package main

import (
	"flag"
	"fmt"
	"log"
	"net"
	"os"
	"os/signal"
	"syscall"
)

func main() {
	f := os.NewFile(3, "signal-parent")
	host, container := parseHostContainerAddrs()

	p, err := NewProxy(host, container)
	if err != nil {
		fmt.Fprintf(f, "1\n%s", err)
		f.Close()
		os.Exit(1)
	}
	go handleStopSignals(p)
	fmt.Fprint(f, "0\n")
	f.Close()

	// Run will block until the proxy stops
	p.Run()
}

// parseHostContainerAddrs parses the flags passed on reexec to create the TCP/UDP/SCTP
// net.Addrs to map the host and container ports
func parseHostContainerAddrs() (host net.Addr, container net.Addr) {
	var (
		proto         = flag.String("proto", "tcp", "proxy protocol")
		hostIP        = flag.String("frontend-ip", "", "frontend ip")
		hostPort      = flag.Int("frontend-port", -1, "frontend port")
		containerIP   = flag.String("backend-ip", "", "backend ip")
		containerPort = flag.Int("backend-port", -1, "backend port")
	)

	flag.Parse()

	switch *proto {
	case "tcp":
		host = &net.TCPAddr{IP: net.ParseIP(*hostIP), Port: *hostPort}
		container = &net.TCPAddr{IP: net.ParseIP(*containerIP), Port: *containerPort}
	case "udp":
		host = &net.UDPAddr{IP: net.ParseIP(*hostIP), Port: *hostPort}
		container = &net.UDPAddr{IP: net.ParseIP(*containerIP), Port: *containerPort}
	default:
		log.Fatalf("unsupported protocol %s", *proto)
	}

	return host, container
}

func handleStopSignals(p Proxy) {
	s := make(chan os.Signal, 10)
	signal.Notify(s, os.Interrupt, syscall.SIGTERM)

	for range s {
		p.Close()

		os.Exit(0)
	}
}
