import { SabaSportApiUrl } from "../../constant/api.js";
import { SingleBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendNewSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId) {
  return sendNewSabaSportWithBetDetailRequest(SingleBetGenerator.generateBetRequest(userId), SabaSportApiUrl.PlaceBet)
}
