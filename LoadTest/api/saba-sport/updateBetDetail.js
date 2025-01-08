import http from "k6/http";
import { apiHost, SabaSportApiUrl } from "../../constant/api.js";

export default function (url, req) {
  let res = http.post(
    url.replace(apiHost().sabaSport, apiHost().sabaSportBetDetail),
    req,
    {
      headers: { "Content-Type": "application/json" },
    }
  );

  return { fullRequest: res };
}
