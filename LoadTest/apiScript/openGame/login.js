const UserInfos = require("../LocalAccountInfo.json");
const cookie = require("cookie");
const { gotoGame } = require("./openGame");

const login = async (userName) => {
  const requestParams = {
    isPersistent: false,
    platform: "Win32",
    screen: "1920:1080:24",
    url: "https://s1-player-site.starixnet.com",
    userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
    userName: userName,
    userpwd: "1234aaAA",
  };

  try {
    const response = await fetch(
      `https://s1-player-site.starixnet.com/api/Site/SignIn`,
      {
        method: "POST",
        headers: {
          Accept: "application/json, text/plain, */*",
          "Accept-Encoding": "gzip, deflate, br, zstd",
          "Accept-Language": "en-US,en;q=0.9",
          "Content-Type": "application/json",
          Host: "s1-player-site.starixnet.com",
          Origin: "https://s1-player-site.starixnet.com/",
          "Origin-Host": "https://s1-player-site.starixnet.com/",
          Referer: "https://s1-player-site.starixnet.com/en",
        },
        body: JSON.stringify(requestParams),
        credentials: "include",
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(errorData);
    }

    return response;
  } catch (error) {
    console.error("Error generating report:", requestParams.userName);
    console.error("respone:", error);
    return null;
  }
};

(async () => {
  for (let i = 0; i < UserInfos.length; i++) {
    await login(UserInfos[i].UserName);
    console.log(`[${i}]`,"user", UserInfos[i].UserName, "loggin success");
  } 

  // for (let i = 0; i < UatAccounts.length; i++) {
  //   const account = UatAccounts[i].UserName;
  //   const response = await login(account);

  //   console.log(`[${i}]`,"user", account, "loggin success");
    
  //   const cookies = response.headers.get("set-cookie");
  //   await Promise.all(
  //     GameOptions.map(
  //       async (option) =>
  //         await gotoGame(
  //           { Cookie: cookies },
  //           {
  //             userName: account,
  //             gameId: option.GameId,
  //             gameType: option.TpGameType,
  //             tpId: option.TpId,
  //           }
  //         )
  //     )
  //   );
  // }
})();

const GameOptions = [
  // {
  //   TpId: "67",
  //   TpGameType: "UuSlots",
  //   GameId: "120",
  // },
  // {
  //   TpId: "68",
  //   TpGameType: "FastSpin",
  //   GameId: "122",
  // },
  // {
  //   TpId: "14",
  //   TpGameType: "PocketGames",
  //   GameId: "17",
  // },
  
  // {
  //   TpId: "12",
  //   TpGameType: "AllBet",
  //   GameId: "6",
  // },
  // {
  //   TpId: "55",
  //   TpGameType: "FaChai",
  //   GameId: "106",
  // },
  // {
  //   TpId: "32",
  //   TpGameType: "JiLi",
  //   GameId: "53",
  // },
  // {
  //   TpId: "68",
  //   TpGameType: "Live22",
  //   GameId: "121",
  // },
  // {
  //   TpId: "72",
  //   TpGameType: "Lottery",
  //   GameId: "125",
  // },
  // {
  //   TpId: "61",
  //   TpGameType: "MaxGame",
  //   GameId: "113",
  // },
  // {
  //   TpId: "5",
  //   TpGameType: "PragmaticPlay",
  //   GameId: "16",
  // },
  // {
  //   TpId: "71",
  //   TpGameType: "Keno",
  //   GameId: "124",
  // },
  // {
  //   TpId: "66",
  //   TpGameType: "SabaPlay",
  //   GameId: "119",
  // },
  // {
  //   TpId: "73",
  //   TpGameType: "TableGame",
  //   GameId: "126",
  // },
  // {
  //   TpId: "15",
  //   TpGameType: "SexyCasino",
  //   GameId: "18",
  // },
  // {
  //   TpId: "12",
  //   TpGameType: "AllBet",
  //   GameId: "6",
  // },
  // {
  //   TpId: "18",
  //   TpGameType: "WorldEntertainment",
  //   GameId: "28",
  // }
  ,{
    TpId: "41",
    TpGameType: "69",
    GameId: "997",
  }
];

const throttlePromises = async (
  tasks,
  limit,
  interval
) => {
  const results = [];
  let currentIndex = 0;

  const executeBatch = async () => {
    const batch = tasks.slice(currentIndex, currentIndex + limit);
    currentIndex += limit;

    // Run the current batch
    const batchResults = await Promise.all(batch.map(task => task()));
    results.push(...batchResults);
  };

  while (currentIndex < tasks.length) {
    await executeBatch();
    if (currentIndex < tasks.length) {
      await new Promise(resolve => setTimeout(resolve, interval));
    }
  }

  return results;
};