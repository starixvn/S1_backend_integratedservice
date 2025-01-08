import clearLoadTestData from "./clearLoadTestData.js";
import confirmbet from "./confirmbet.js";
import confirmbetParlay from "./confirmbetParlay.js";
import getBalance from "./get-balance.js";
import placebet from "./placebet.js";
import placebetParlay from "./placebetParlay.js";
import settle from "./settle.js";

const SabaSportApi = {
    confirmbet,
    confirmbetParlay,
    getBalance,
    placebet,
    placebetParlay,
    settle,
    clearLoadTestData
}

export default SabaSportApi