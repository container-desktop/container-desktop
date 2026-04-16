ARG DOCKER_VERSION="28.3.0"
FROM ubuntu:24.04
# Retry failed apt downloads up to 3 times to survive transient mirror sync issues
RUN echo 'Acquire::Retries "3";' > /etc/apt/apt.conf.d/80-retries
RUN apt-get update -y && \
    apt-get install -y --fix-missing ca-certificates curl gnupg lsb-release unzip zip && \
    rm -rf /var/lib/apt/lists/*
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
RUN echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
RUN apt-get update -y && \
    apt-get install -y --fix-missing docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*