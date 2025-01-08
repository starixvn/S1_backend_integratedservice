const UatAccounts = require("../UatAccounts.json");
const { getCookie } = require("../utils/getCookie");

const addMoney = async (custId) => {
  const requestParams = {
    custId: custId,
    adjustmentType: 1,
    amount: 10000000,
    withdrawalLimitAmount: 0,
    withdrawalLimitOption: 0,
    reasonId: 3,
    memo: "",
    methodType: 5,
  };

  try {
    const response = await fetch(
      `https://boapi.vgamesuat.com/apiV2/BalanceAdjustment/Submit`,
      {
        method: "POST",
        headers: {
          accept: "application/json, text/plain, */*",
          "cache-control": "no-cache",
          "content-type": "application/json",
          cookie: getCookie(),
        },
        body: JSON.stringify(requestParams),
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Error: ${errorData}`);
    }
    return requestParams;
    
  } catch (error) {
    console.error("Error generating report:", requestParams.userName);
    console.error("respone:", error);
    return null;
  }
};

(async () => {
  for (let i = 2272; i < UatAccounts.length; i++) {
      const res = await addMoney(UatAccounts[i].CustId)
      console.log(`${i}:`, `+${res.amount}`, 'for user:', res.custId)
    }
})()