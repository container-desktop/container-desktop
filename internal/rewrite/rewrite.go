package rewrite

import (
	"encoding/json"
	"io"
	"regexp"
	rt "runtime"
	"strings"

	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
)

type rewriteMapItem struct {
	method   string
	pattern  string
	rewriter func(map[string]interface{}, string)
}

var rewriteMappings = []rewriteMapItem{
	{"GET", `(/.*?)?/containers(/.*?)?/json`, rewriteContainerSummary},
	{"POST", `(/.*?)?/containers/create`, rewriteContainerConfig},
	{"POST", `(/.*?)?/services/create`, rewriteServiceSpec},
	{"POST", `(/.*?)?/services/(/.*?)/update`, rewriteServiceSpec},
	{"GET", `(/.*?)?/services(/.*?)$`, rewriteService},
	{"GET", `(/.*?)?/tasks(/.*?)?`, rewriteTask},
}

func enabled(logger *zap.SugaredLogger, level zapcore.Level) bool {
	return logger.Desugar().Core().Enabled(level)
}

func RewriteBody(body io.ReadCloser, urlPath string, wslDistroName string, logger *zap.SugaredLogger) (rewrittenBody []byte, err error) {
	if body != nil {
		rewriter, ok := getRewriter(urlPath)
		if ok {
			buf, err := io.ReadAll(body)
			if enabled(logger, zapcore.DebugLevel) {
				logger.Debugf("Original body: %s", string(buf))
			}
			if err != nil {
				return nil, err
			}
			if len(buf) == 0 {
				return buf, nil
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
				if len(wslDistroName) > 0 {
					path += "wsl/" + wslDistroName
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

				if enabled(logger, zapcore.DebugLevel) {
					logger.Debugf("Rewritten body: %s", string(buf))
				}
				return buf, nil
			}
		}
	}
	return nil, nil
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
	o, ok = jsonMap["Mounts"]
	if ok {
		mounts, ok := o.([]interface{})
		if ok {
			rewriteMounts(mounts, path)
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
	o, ok = hostConfig["Mounts"]
	if ok {
		mounts, ok := o.([]interface{})
		if ok {
			rewriteMounts(mounts, path)
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
