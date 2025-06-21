const mongoose = require('mongoose');

const tradeSchema = new mongoose.Schema({
  instrument: {
    type: String,
    required: true
  },
  action: {
    type: String,
    required: true,
    enum: ['Buy', 'Sell']
  },
  quantity: {
    type: Number,
    required: true
  },
  price: {
    type: Number,
    required: true
  },
  time: {
    type: Date,
    required: true
  },
  id: {
    type: String,
    required: true
  },
  entryExit: {
    type: String,
    required: true,
    enum: ['Entry', 'Exit']
  },
  position: String,
  orderId: String,
  name: String,
  commission: {
    type: Number,
    default: 0
  },
  rate: Number,
  account: String,
  connection: String,
  uploadDate: {
    type: Date,
    default: Date.now
  }
}, {
  timestamps: true
});

module.exports = mongoose.model('Trade', tradeSchema);
