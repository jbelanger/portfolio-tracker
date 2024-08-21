import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'http://localhost:5262', // Replace with your API base URL
  headers: {
    'Content-Type': 'application/json',
  },
});

export default apiClient;
