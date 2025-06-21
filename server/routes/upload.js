const express = require('express');
const multer = require('multer');
const path = require('path');
const fs = require('fs');
const { parseCsvFile, saveTradesToDB } = require('../utils/csvParser');
const { pairTrades } = require('../utils/tradePairing');

const router = express.Router();

// Configure multer for file uploads
const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    const uploadDir = path.join(__dirname, '../uploads');
    if (!fs.existsSync(uploadDir)) {
      fs.mkdirSync(uploadDir, { recursive: true });
    }
    cb(null, uploadDir);
  },
  filename: (req, file, cb) => {
    cb(null, Date.now() + '-' + file.originalname);
  }
});

const upload = multer({
  storage,
  fileFilter: (req, file, cb) => {
    if (file.mimetype === 'text/csv' || path.extname(file.originalname) === '.csv') {
      cb(null, true);
    } else {
      cb(new Error('Only CSV files are allowed'));
    }
  }
});

// Upload CSV file
router.post('/csv', upload.single('csvFile'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'No file uploaded' });
    }

    const filePath = req.file.path;

    // Parse CSV file
    const trades = await parseCsvFile(filePath);

    if (trades.length === 0) {
      return res.status(400).json({ error: 'No valid trades found in CSV file' });
    }

    // Save trades to database
    const savedTrades = await saveTradesToDB(trades);

    // Pair the trades
    const pairedTrades = await pairTrades();

    // Clean up uploaded file
    fs.unlinkSync(filePath);

    res.json({
      message: 'CSV uploaded and processed successfully',
      tradesCount: savedTrades.length,
      pairedTradesCount: pairedTrades.length,
      trades: savedTrades,
      pairedTrades
    });

  } catch (error) {
    console.error('Upload error:', error);
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
