import { SabaSportApiUrl } from "../../constant/api.js";
import { ParlayBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendNewSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId) {
  return sendNewSabaSportWithBetDetailRequest(ParlayBetGenerator.getPlaceBetParlayRequest(userId), SabaSportApiUrl.PlaceBetParlay)
}
