import http from "k6/http";
import { PlayerSiteApiUrl } from "../../constant/api.js";
import { objectToQueryParams, ParseBodyToJson } from "../../data-generator/clientGenerator.js";

export default function ({ tpId, gameType, gameId }) {
  const requestParams = {
    tpId: tpId,
    LangId: "en",
    tpId: tpId,
    tpGameType: gameType,
    signupUrl: `${PlayerSiteApiUrl.Root}/join`,
    loginUrl: `${PlayerSiteApiUrl.Root}/login`,
    homeUrl: PlayerSiteApiUrl.Root,
    gameId: gameId,
    errorUrl: `${PlayerSiteApiUrl.Root}/404/index_en.html`,
    cashierUrl: `${PlayerSiteApiUrl.Root}/deposit`,
  };

  let res = http.get(`${PlayerSiteApiUrl.OpenGame}?${objectToQueryParams(requestParams)}`, {
    headers: {
      Accept: "application/json, text/plain, */*",
      "Accept-Encoding": "gzip, deflate, br, zstd",
      "Accept-Language": "en-US,en;q=0.9",
      "Content-Type": "application/json",
      Host: "s1-member-site.starixnet.com",
      Origin: PlayerSiteApiUrl.Root,
      "Origin-Host": PlayerSiteApiUrl.Root,
      Referer: `${PlayerSiteApiUrl.Root}/en`,
    }
  });

  return { request: requestParams, fullRequest: res };
}
