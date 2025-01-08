import { SabaSportApiUrl } from "../../constant/api.js";
import { SingleBetGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaSportWithBetDetailRequest } from "../../data-generator/clientGenerator.js";

export default function (userId) {
  return sendSabaSportWithBetDetailRequest(
    SingleBetGenerator.generateBetRequest(userId),
    SabaSportApiUrl.PlaceBet
  );
}
