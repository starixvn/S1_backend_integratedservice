const fs = require("fs");
const { getCookie } = require("../utils/getCookie");

async function adduser(id, phonenum) {
  const userdata = {
    siteId: 1000,
    userName: "nsDemoloaduser" + id,
    userpwd: "1234aaAA",
    memberType: 1,
    email: `nsDemouatloaduser${id}@gmail.com`,
    currencyId: 4,
    areaCode: "+66",
    phoneNo: phonenum,
    realName: "loaduser",
  };

  try {
    const response = await fetch(`https://boapi.vgamesuat.com/apiV2/Member/RegisterMember`,
      {
        method: "POST",
        headers: {
          accept: "application/json, text/plain, */*",
          "cache-control": "no-cache",
          "content-type": "application/json",
          cookie: getCookie()
        },
        body: JSON.stringify(userdata),
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Error: ${errorData}`);
    }

    return userdata;
  } catch (error) {
    console.error("Error generating report:", userdata.userName);
    console.error("respone:", error);
    return null;
  }
}

(async () => {
  try {
    function generateRandomNumber(length) {
      const min = Math.pow(10, length - 1);
      const max = Math.pow(10, length) - 1;
      return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    let playercode = [];
    //SET NUMBER
    for (let index = 3001; index <= 4000; index++) {
      const fnum = generateRandomNumber(9);
      const data = await adduser(index, fnum);
      if (data) {
        console.log(data.userName);
        playercode.push(data.userName);
      }
    }

    console.log(playercode);

    const jsonString = JSON.stringify(playercode, null, 2);

    // Write JSON string to a file
    fs.writeFile("UatAccounts.json", jsonString, "utf8", (err) => {
      if (err) {
        console.error("Error writing to file", err);
      } else {
        console.log("Array written to file successfully");
      }
    });
  } catch (error) {
    console.error("Error in the main flow:", error);
  }
})();
