import http from "k6/http";
import { PlayerSiteApiUrl } from "../../constant/api.js";
import { ParseBodyToJson } from "../../data-generator/clientGenerator.js";

export default function (userName) {
  const request = {
    isPersistent: false,
    platform: "Win32",
    screen: "1920:1080:24",
    url: `${PlayerSiteApiUrl.Root}`,
    userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
    userName: userName,
    userpwd: "1234aaAA",
  }

  let res = http.post(PlayerSiteApiUrl.Login, JSON.stringify(request), {
    headers: {
        Accept: "application/json, text/plain, */*",
        "Accept-Encoding": "gzip, deflate, br, zstd",
        "Accept-Language": "en-US,en;q=0.9",
        "Content-Type": "application/json",
        Host: PlayerSiteApiUrl.Domain,
        Origin: `${PlayerSiteApiUrl.Root}/`,
        "Origin-Host": `${PlayerSiteApiUrl.Root}/`,
        Referer: `${PlayerSiteApiUrl.Root}/en`,
    },
  });

  return { request: request, response: ParseBodyToJson(res), fullRequest: res }
}
