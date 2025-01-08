# Load Testing Guide

This document outlines the steps to perform load testing on your application using `k6` with Docker and directly via the command line.

## Prerequisites

1. **Docker**: Ensure that Docker is installed and running on your machine. You can download it from [Docker's official website](https://www.docker.com/get-started).
2. **k6**: This guide uses `k6`, a modern load testing tool. Ensure that it is installed on your system. You can find installation instructions on the [k6 website](https://k6.io/docs/getting-started/installation/).

## Load Testing Commands

You can run a load test using `k6` in two ways:

### Method 1: Using Docker

#### Simple Test

```bash
docker run --rm -i -v ${PWD}/LoadTests:/app -w /app grafana/k6 run -e RUN_MODE=DEV script.js
```

#### Load Test

```bash
docker run --rm -i -v ${PWD}/LoadTests:/app -w /app grafana/k6 run -e RUN_MODE=CONSTANT_RATE script.js
```

### Method 2: Using k6 Directly

#### Simple Test

```bash
k6 run -e RUN_MODE=DEV -e OPTIONS=SabaSport scripts/saba-sport/script.js
k6 run -e RUN_MODE=DEV -e OPTIONS=SabaPlus scripts/saba-plus/script.js
```

#### Load Test

```bash
k6 run -e RUN_MODE=CONSTANT_RATE -e OPTIONS=SabaSport scripts/saba-sport/script.js
k6 run -e RUN_MODE=CONSTANT_RATE -e OPTIONS=SabaPlus scripts/saba-plus/script.js
```