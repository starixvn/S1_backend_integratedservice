import { SabaPlusGenerator } from "../../data-generator/requestGenerator.js";
import { SabaPlusApiUrl } from "../../constant/api.js";
import { sendSabaPlusRequest } from "../../data-generator/clientGenerator.js";

export default function (placebetRequest) {
  return sendSabaPlusRequest(SabaPlusGenerator.getEndGameSBPlusRequest(placebetRequest), SabaPlusApiUrl.EndGame)
}
