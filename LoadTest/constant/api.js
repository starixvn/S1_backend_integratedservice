const defaultHost = {
  sabaSport: "https://third-party-sw.starixnet.com/sbsw/Starix1SW",
  sabaSportBetDetail: "https://localhost:7078/api/SabaSport",
  sabaPlus: "https://third-party-sw.starixnet.com/maxgame/nova88",
  playerSite: "https://s1-player-site.starixnet.com",
};

export const apiHost = () => {
  if (!__ENV.RUN_ENV) {
    return defaultHost;
  }

  switch (true) {
    case __ENV.RUN_ENV.toLowerCase() == "development":
      return {
        ...defaultHost,
        sabaSport: "https://localhost:7015/api/SabaSport",
        sabaPlus: "https://localhost:7015/api/SabaPlus",
      };
    case __ENV.RUN_ENV.toLowerCase() == "local":
      return {
        ...defaultHost,
        sabaSport: "https://third-party-sw.starixnet.com/sbsw/Starix1SW",
        sabaPlus: "https://third-party-sw.starixnet.com/maxgame/nova88",
        playerSite: "https://s1-player-site.starixnet.com",
      };
    case __ENV.RUN_ENV.toLowerCase() == "uat":
      return {
        ...defaultHost,
        sabaSport: "https://tp.vgamesuat.com/sbsw/NSDemo1SW",
        sabaSportBetDetail: "https://localhost:7079/api/SabaSport",
        sabaPlus: "https://tp.vgamesuat.com/maxgame/nsDemo",
        playerSite: "https://player1.vgamesuat.com"
      };
    default:
      return defaultHost;
  }
};

export const BetDetailApiUrl = {
  AddBetDetail: `${apiHost().sabaSportBetDetail}/BetDetail`,
  Root: apiHost().sabaSportBetDetail,
}

export const PlayerSiteApiUrl = {
  Root: apiHost().playerSite,
  Domain: apiHost().playerSite.replace('https://', '').replace('http://', ''),
  Login: `${apiHost().playerSite}/api/Site/SignIn`,
  OpenGame: `${apiHost().playerSite}/api/entPage`,
};

export const SabaSportApiUrl = {
  GetBalance: `${apiHost().sabaSport}/getbalance`,
  PlaceBet: `${apiHost().sabaSport}/placebet`,
  ConfirmBet: `${apiHost().sabaSport}/confirmbet`,
  PlaceBetParlay: `${apiHost().sabaSport}/placebetparlay`,
  ConfirmBetParlay: `${apiHost().sabaSport}/confirmbetparlay`,
  Settle: `${apiHost().sabaSport}/settle`,
  ClearLoadTestData: `${apiHost().sabaSport}/ClearLoadTestData`,
};

export const SabaPlusApiUrl = {
  GetBalance: `${apiHost().sabaPlus}/getbalance`,
  PlaceBet: `${apiHost().sabaPlus}/placebet`,
  EndGame: `${apiHost().sabaPlus}/endgame`,
  PlaceBetAndSettle: `${apiHost().sabaPlus}/PlaceBetAndSettle`,
  FreeSpin: `${apiHost().sabaPlus}/FreeSpin`,
  VoidGame: `${apiHost().sabaPlus}/VoidGame`,
  GetPlayerInfo: `${apiHost().sabaPlus}/GetPlayerInfo`,
};
