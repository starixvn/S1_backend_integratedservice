import { SabaPlusApiUrl } from "../../constant/api.js";
import { SabaPlusGenerator } from "../../data-generator/requestGenerator.js";
import { sendSabaPlusRequest } from "../../data-generator/clientGenerator.js";

export default function({operatorCustId, gameType, gameId}) {
  const request = SabaPlusGenerator.getPlayerInfoRequest({operatorCustId, gameType, gameId});
  return sendSabaPlusRequest(request, SabaPlusApiUrl.GetPlayerInfo)
}
