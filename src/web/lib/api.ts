// lib/api.ts
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL as string;

export const fetcher = async (
  url: string,
  options: RequestInit = {}
): Promise<any> => {
  const response = await fetch(`${API_BASE_URL}${url}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    },
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || 'An error occurred while fetching data.');
  }

  return response.json();
};
