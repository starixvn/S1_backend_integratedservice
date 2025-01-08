const fs = require('fs');
const path = require('path');

const gotoGame = async (header, { userName, tpId, gameType, gameId }) => {
  const requestParams = {
    LangId: "en",
    tpId: tpId,
    tpGameType: gameType,
    signupUrl: "https://player1.vgamesuat.com/join",
    loginUrl: "https://player1.vgamesuat.com/login",
    homeUrl: "https://player1.vgamesuat.com",
    gameId: gameId,
    errorUrl: "https://player1.vgamesuat.com/404/index_en.html",
    cashierUrl: "https://player1.vgamesuat.com/deposit",
  };

  const queryParams = new URLSearchParams(requestParams);

  const url = `https://player1.vgamesuat.com/api/entPage?${queryParams.toString()}`;
  try {
    const response = await fetch(url, {
      method: "GET",
      headers: {
        ...header,
        Accept: "application/json, text/plain, */*",
        "Accept-Encoding": "gzip, deflate, br, zstd",
        "Accept-Language": "en-US,en;q=0.9",
        "Content-Type": "application/json",
        Host: "s1-member-site.starixnet.com",
        Origin: "https://player1.vgamesuat.com/",
        "Origin-Host": "https://player1.vgamesuat.com/",
        Referer: "https://player1.vgamesuat.com/en",
      },
    });

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`status ${response.status}, game ${gameId}`);
    }

    console.log("user", userName, `open game ${gameId} success`);
  } catch (error) {
    console.error("Error generating report:", userName, 'gameId', gameId);
    console.error("respone:", error);

    const logger = createExceptionLogger('./gotogame.log')
    logger(`Error generating report: ${userName}, gameId ${gameId}\n`)
    logger(`respone: ${error}\n`)
    return null;
  }
};

const createExceptionLogger = (filePath) => {
  const logFile = path.resolve(filePath);

  return (error) => {
    const timestamp = new Date().toISOString();
    const logEntry = `[${timestamp}] [ERROR]: ${error};`

    fs.appendFile(logFile, logEntry, (err) => {
      if (err) {
        console.error(`Failed to write to log file: ${err.message}`);
      }
    });
  };
};

module.exports = { gotoGame };
