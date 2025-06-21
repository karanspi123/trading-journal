import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const uploadCsv = async (file) => {
  const formData = new FormData();
  formData.append('csvFile', file);

  const response = await api.post('/upload/csv', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response.data;
};

export const getPairedTrades = async () => {
  const response = await api.get('/trades/paired');
  return response.data;
};

export const getChartData = async (instrument) => {
  const response = await api.get(`/trades/chart/${instrument}`);
  return response.data;
};

export const generateTradeSummary = async (tradeId) => {
  const response = await api.post(`/trades/summary/${tradeId}`);
  return response.data;
};

export const clearAllTrades = async () => {
  const response = await api.delete('/trades/clear');
  return response.data;
};

export default api;
