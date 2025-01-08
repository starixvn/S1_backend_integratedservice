import http from "k6/http";
import { SabaSportApiUrl } from "../../constant/api.js";
import { SingleBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendNewSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId, confimBetRequest) {
  return sendNewSabaSportWithBetDetailRequest(SingleBetGenerator.getSettlleBetRequest(
    userId,
    confimBetRequest
  ), SabaSportApiUrl.Settle)
}
