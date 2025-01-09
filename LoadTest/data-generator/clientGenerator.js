import http from "k6/http";
import crypto from "k6/crypto";
import { check, fail } from "k6";
import updateBetDetail from "../api/saba-sport/updateBetDetail.js";

export const encryptWithOperatorKey = (jsonRequest, operatorKey) => {
  const dataToHash = jsonRequest + (operatorKey || "");

  // Compute MD5 hash
  const hash = crypto.createHash("md5");
  hash.update(dataToHash, "utf-8");
  return hash.digest("hex");
};

export const ParseBodyToJson = (request) => {
  try {
    return JSON.parse(request.body);
  } catch (e) {
    console.log(request);
    return null;
  }
};

const SabaSportOperatorKey = {
  local: 'y2g53zl9g4',
  uat: 'qz7vzb5uea'
}

export const sendSabaSportWithBetDetailRequest = (request, url) => {
  request.key = SabaSportOperatorKey[__ENV.RUN_ENV?.toLowerCase() ?? 'local'] ?? SabaSportOperatorKey.local;
  const jsonRequest = JSON.stringify(request);
  let res = http.post(url, jsonRequest, {
    headers: { "Content-Type": "application/json" },
    timeout: "300s"
  });

  updateBetDetail(url, jsonRequest);

  return { request: request, response: ParseBodyToJson(res), fullRequest: res };
};

export const sendNewSabaSportWithBetDetailRequest = (request, url) => {
  request.key = SabaSportOperatorKey[__ENV.RUN_ENV?.toLowerCase() ?? 'local'] ?? SabaSportOperatorKey.local;

  let res = http.post(url, JSON.stringify(request), {
    headers: { "Content-Type": "application/json" },
    timeout: "300s"
  });

  return { request: request, response: ParseBodyToJson(res), fullRequest: res };
};

const SabaPlusOperatorKey = {
  local: 'ck03ijvk',
  uat: 'chii0lcy'
}

export const sendSabaPlusRequest = (request, url) => {
  request.OperatorKey = SabaPlusOperatorKey[__ENV.RUN_ENV?.toLowerCase() ?? 'local'] ?? SabaPlusOperatorKey.local;

  const jsonRequest = JSON.stringify(request);

  let res = http.post(url, jsonRequest, {
    headers: {
      "Content-Type": "application/json",
      "operator-sign": encryptWithOperatorKey(jsonRequest, request.OperatorKey),
      timeout: "300s"
    },
  });

  return { request: request, response: ParseBodyToJson(res), fullRequest: res };
};

export const objectToQueryParams = (obj) => {
  return Object.keys(obj)
    .map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(obj[key])}`)
    .join("&");
};

export const checkSabaPlusResponseError = (res, message) => {
  const logMessage = `${message} content error code is 0`;
  const resCheck = check(res.response, {
    [logMessage]: (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!resCheck) {
    console.log(logMessage, res.fullRequest);
    fail(`${message} failed`);
  }
};

export const checkSabaSportResponseError = (res, message) => {
  const logMessage = `${message} content status code is 0`;
  const resCheck = check(res.response, {
    [logMessage]: (r) => r?.status && r.status === "0",
  });

  if (!resCheck) {
    console.log(logMessage, res.fullRequest);
    fail(`${message} failed`);
  }
};

export const getResponseError = (obj, ...fieldName) => {
  for (let i = 0; i < fieldName.length; ++i) {
    if (obj[fieldName[i]]) {
      return obj[fieldName[i]];
    }
  }
};
