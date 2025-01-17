
const Scenarios = {
  local: {
    DEV: {
      per_vus_iterations_test: {
        executor: "per-vu-iterations",
        vus: 3, 
        iterations: 2, 
      },
    },
    CONSTANT_RATE: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 50,
        timeUnit: "10s",
        duration: "4m",
        preAllocatedVUs: 200,
        gracefulStop: '90s',
        maxVUs: 500,
      },
    },
    RAMPING_RATE: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 2,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 6, duration: "1m" },
          { target: 8, duration: "2m" },
          { target: 10, duration: "3m30s" },
          { target: 0, duration: "1m" },
        ],
      },
    },
  },
  uat: {
    DEV: {
      per_vus_iterations_test: {
        executor: "per-vu-iterations",
        vus: 40, // 2 virtual user
        iterations: 2, // The user will send 2 requests
      },
    },
    CONSTANT_RATE_DEAD_LOCK: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 200,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 120,
        maxVUs: 1000,
        gracefulStop: '90s',
        env: { MY_CROC_ID: "1" },
        tags: {
          test_type: "api_load_test",
        },
      },
    },
    CONSTANT_RATE_60: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 150,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 120,
        maxVUs: 1000,
        gracefulStop: '90s',
      },
    },
    CONSTANT_RATE_90: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 225,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 120,
        maxVUs: 1000,
        gracefulStop: '90s',
      },
    },
    CONSTANT_RATE_100: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 250,
        timeUnit: "10s",
        duration: "2m",
        preAllocatedVUs: 120,
        maxVUs: 1000,
        gracefulStop: '90s',
      },
    },
    CONSTANT_RATE_120: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 300,
        timeUnit: "10s",
        duration: "2m",
        preAllocatedVUs: 120,
        maxVUs: 1000,
        gracefulStop: '90s',
      },
    },
    CONSTANT_RATE_150: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 300,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 120,
        maxVUs: 500,
        gracefulStop: '90s',
      },
    },
    RAMPING_RATE_60: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 5,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 10, duration: "30s" },
          { target: 15, duration: "30s" },
          { target: 15, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
	RAMPING_RATE_80: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 5,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 10, duration: "30s" },
          { target: 20, duration: "30s" },
          { target: 20, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
    RAMPING_RATE_100: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 15,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 20, duration: "30s" },
          { target: 25, duration: "30s" },
          { target: 25, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
    RAMPING_RATE_120: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 15,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 23, duration: "30s" },
          { target: 30, duration: "30s" },
          { target: 30, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
	RAMPING_RATE_240: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 25,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 40, duration: "30s" },
          { target: 60, duration: "30s" },
          { target: 60, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
    RAMPING_RATE: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 5,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        gracefulStop: '90s',
        stages: [
          { target: 10, duration: "1m" },
          { target: 15, duration: "1m" },
          { target: 20, duration: "1m" },
          { target: 25, duration: "3m30s" },
          { target: 0, duration: "30s" },
        ],
      },
    },
  },
}

export const SabaSportScenarios = Scenarios[__ENV.RUN_ENV?.toLowerCase() ?? 'local']
