import { useSession } from 'next-auth/react';

const baseURL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:5262';


const getHeaders = function () {
  // const { data: session } = useSession();
  return ({
    'Content-Type': 'application/json',
    //'Authorization': `Bearer ${session?.accessToken}`,
  });
};

const apiConfig = {
  baseURL,
  getHeaders
};

export default apiConfig;
