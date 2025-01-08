import { SabaSportApiUrl } from "../../constant/api.js";
import { ParlayBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (placeBetRequest, placeBetResponse) {
  return sendSabaSportWithBetDetailRequest(
    ParlayBetGenerator.getConfirmBetParlayRequest(
      placeBetRequest,
      placeBetResponse
    ),
    SabaSportApiUrl.ConfirmBetParlay
  );
}
