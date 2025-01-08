const fs = require('fs');
const csv = require('csv-parser');

const results = [];

// Read the CSV file and convert to JSON
fs.createReadStream('./UatAccounts.csv') // Replace with the path to your CSV file
  .pipe(csv())
  .on('data', (row) => {
    results.push(row); // Add each CSV row as a JSON object
  })
  .on('end', () => {
    fs.writeFile('UatAccounts.json', JSON.stringify(results, null, 2), (err) => {
        if (err) {
          console.error('Error writing to JSON file:', err);
          return;
        }
        console.log('JSON data saved to output.json');
      });

    console.log('Process Done:');
  })
  .on('error', (err) => {
    console.error('Error reading the file:', err);
  });
