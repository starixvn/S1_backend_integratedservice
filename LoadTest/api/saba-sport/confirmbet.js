import { SabaSportApiUrl } from "../../constant/api.js";
import { SingleBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId, placeBetResponse) {
  return sendSabaSportWithBetDetailRequest(
    SingleBetGenerator.getConfirmBetRequest(userId, placeBetResponse),
    SabaSportApiUrl.ConfirmBet
  );
}
