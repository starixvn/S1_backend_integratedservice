const Scenarios = {
  local: {
    DEV: {
      per_vus_iterations_test: {
        executor: "per-vu-iterations",
        vus: 1, // 2 virtual user
        iterations: 1, // The user will send 2 requests
      },
    },
    SUPER_RAMPAGE_RATE: {
      constant_rate_test: {
        executor: "per-vu-iterations",
        vus: 400,
        iterations: 1,
      },
    },
    PLACE_BET_RATE: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 50,
        timeUnit: "1s",
        duration: "2m",
        preAllocatedVUs: 500,
        maxVUs: 500,
      },
    },
    CONSTANT_RATE: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 120,
        timeUnit: "10s",
        duration: "4m",
        preAllocatedVUs: 50,
        maxVUs: 500,
      },
    },
    RAMPING_RATE: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 6,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 8, duration: "1m" },
          { target: 15, duration: "3m30s" },
          { target: 0, duration: "1m" },
        ],
      },
    },
  },
  uat: {
    DEV: {
      per_vus_iterations_test: {
        executor: "per-vu-iterations",
        vus: 3,
        iterations: 2,
      },
    },
    CONSTANT_RATE_DEAD_LOCK: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 100,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 5000,
        env: { MY_CROC_ID: "1" },
        tags: {
          test_type: "api_load_test",
        },
      },
    },
    CONSTANT_RATE_REDEPLOY: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 200,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_120: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 400,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_130: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 433,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_140: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 467,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_150: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 500,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_180: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 600,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    CONSTANT_RATE_240: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 800,
        timeUnit: "10s",
        duration: "5m",
        preAllocatedVUs: 100,
        maxVUs: 300,
      },
    },
    RAMPING_RATE_120: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 25,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 30, duration: "30s" },
          { target: 40, duration: "30s" },
          { target: 40, duration: "5m" },
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
    RAMPING_RATE_150: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 25,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 35, duration: "30s" },
          { target: 50, duration: "30s" },
          { target: 40, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
    RAMPING_RATE: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 20,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 30, duration: "30s" },
          { target: 30, duration: "2m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
  },
};

export const SabaPlusScenarios =
  Scenarios[__ENV.RUN_ENV?.toLowerCase() ?? "local"];
