import { SabaSportApiUrl } from "../../constant/api.js";
import { ParlayBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId) {
  return sendSabaSportWithBetDetailRequest(
    ParlayBetGenerator.getPlaceBetParlayRequest(userId),
    SabaSportApiUrl.PlaceBetParlay
  );
}
