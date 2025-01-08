import { SabaPlusApiUrl } from "../../constant/api.js";
import { SabaPlusGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaPlusRequest } from "../../data-generator/clientGenerator.js";

export default function(operatorCustId) {
  return sendSabaPlusRequest(SabaPlusGenerator.getPlaceBetAndSettleSBPlusRequest(operatorCustId), SabaPlusApiUrl.PlaceBetAndSettle)
}
