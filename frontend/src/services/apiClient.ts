import {
  ClassItem,
  HoldResponse,
  HoldStatusResponse,
  ConfirmHoldResponse,
  StudentHoldItem,
  ApiError
} from '../types/api';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');

class ApiClientError extends Error {
  public status: number;
  public details?: ApiError;

  constructor(status: number, message: string, details?: ApiError) {
    super(message);
    this.name = 'ApiClientError';
    this.status = status;
    this.details = details;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE_URL}${path}`;
  const headers = new Headers(options.headers || {});

  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(url, {
    ...options,
    headers
  });

  if (!response.ok) {
    let errorData: ApiError | undefined;
    let errorMessage = `HTTP Error ${response.status}: ${response.statusText}`;

    try {
      errorData = await response.json();
      if (errorData?.detail) {
        errorMessage = errorData.detail;
      }
    } catch {
      // Body not JSON
    }

    throw new ApiClientError(response.status, errorMessage, errorData);
  }

  // Handle 204 No Content (empty body)
  if (response.status === 204) {
    return null as T;
  }

  return response.json() as Promise<T>;
}

export const apiClient = {
  getBaseUrl(): string {
    return API_BASE_URL;
  },

  async getClasses(): Promise<ClassItem[]> {
    return request<ClassItem[]>('/classes');
  },

  async getClassById(classId: string): Promise<ClassItem> {
    return request<ClassItem>(`/classes/${encodeURIComponent(classId)}`);
  },

  async createHold(classId: string, studentId: string): Promise<HoldResponse> {
    return request<HoldResponse>(`/classes/${encodeURIComponent(classId)}/holds`, {
      method: 'POST',
      headers: {
        'X-Student-Id': studentId
      }
    });
  },

  async getHold(holdId: string): Promise<HoldStatusResponse> {
    return request<HoldStatusResponse>(`/holds/${encodeURIComponent(holdId)}`);
  },

  async confirmHold(holdId: string, studentId: string): Promise<ConfirmHoldResponse> {
    return request<ConfirmHoldResponse>(`/holds/${encodeURIComponent(holdId)}/confirm`, {
      method: 'POST',
      headers: {
        'X-Student-Id': studentId
      }
    });
  },

  async cancelHold(holdId: string, studentId: string): Promise<void> {
    return request<void>(`/holds/${encodeURIComponent(holdId)}`, {
      method: 'DELETE',
      headers: {
        'X-Student-Id': studentId
      }
    });
  },

  async getMyHolds(studentId: string): Promise<StudentHoldItem[]> {
    return request<StudentHoldItem[]>('/me/holds', {
      headers: {
        'X-Student-Id': studentId
      }
    });
  }
};

export { ApiClientError };
