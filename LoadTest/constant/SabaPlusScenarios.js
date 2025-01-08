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
        rate: 400,
        timeUnit: "10s",
        duration: "2m",
        preAllocatedVUs: 10,
        maxVUs: 300,
        env: { MY_CROC_ID: "1" },
        tags: {
          test_type: "api_load_test",
        },
      },
    },
    CONSTANT_RATE: {
      constant_rate_test: {
        executor: "constant-arrival-rate",
        rate: 600,
        timeUnit: "10s",
        duration: "1m",
        preAllocatedVUs: 10,
        maxVUs: 300,
        env: { MY_CROC_ID: "1" },
        tags: {
          test_type: "api_load_test",
        },
      },
    },
    RAMPING_RATE: {
      ramping_rate_test: {
        executor: "ramping-arrival-rate",
        startRate: 30,
        timeUnit: "1s",
        preAllocatedVUs: 100,
        maxVUs: 800,
        stages: [
          { target: 40, duration: "30s" },
          { target: 40, duration: "5m" },
          { target: 50, duration: "30s" },
          { target: 50, duration: "5m" },
          { target: 60, duration: "30s" },
          { target: 60, duration: "5m" },
          { target: 0, duration: "30s" },
        ],
      },
    },
  },
}

export const SabaPlusScenarios = Scenarios[__ENV.RUN_ENV?.toLowerCase() ?? 'local']
