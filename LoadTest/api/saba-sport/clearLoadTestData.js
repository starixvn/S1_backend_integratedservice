import http from "k6/http";
import { SabaSportApiUrl } from "../../constant/api.js";

export default function () {
  let res = http.post(SabaSportApiUrl.ClearLoadTestData, null, {
    headers: { "Content-Type": "application/json" },
  });
}
