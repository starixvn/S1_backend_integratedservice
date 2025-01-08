import http from "k6/http";
import { SabaSportApiUrl } from "../../constant/api.js";
import { SingleBetGenerator } from "../../data-generator/requestGenerator.js";
import { ParseBodyToJson } from "../../data-generator/clientGenerator.js";

export default function (userId) {
  const request = SingleBetGenerator.getBalanceRequest(userId);
  let res = http.post(SabaSportApiUrl.GetBalance, JSON.stringify(request), {
    headers: { "Content-Type": "application/json" },
  });

  return { request: request, response: ParseBodyToJson(res), fullRequest: res }
}
