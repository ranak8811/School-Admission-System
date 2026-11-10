import {
  ClassItem,
  HoldResponse,
  HoldStatusResponse,
  ConfirmHoldResponse,
  StudentHoldItem,
  ApiError
} from '../types/api';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');

let activeStudentId: string | null = null;

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

  // Automatically attach active student ID if not explicitly specified in headers
  if (!headers.has('X-Student-Id') && activeStudentId) {
    headers.set('X-Student-Id', activeStudentId);
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

  setStudentId(id: string | null): void {
    activeStudentId = id && id.trim().length > 0 ? id.trim() : null;
  },

  getStudentId(): string | null {
    return activeStudentId;
  },

  async getClasses(): Promise<ClassItem[]> {
    return request<ClassItem[]>('/classes');
  },

  async getClassById(classId: string): Promise<ClassItem> {
    return request<ClassItem>(`/classes/${encodeURIComponent(classId)}`);
  },

  async createHold(classId: string, studentId?: string): Promise<HoldResponse> {
    const id = studentId || activeStudentId || '';
    return request<HoldResponse>(`/classes/${encodeURIComponent(classId)}/holds`, {
      method: 'POST',
      headers: {
        'X-Student-Id': id
      }
    });
  },

  async getHold(holdId: string): Promise<HoldStatusResponse> {
    return request<HoldStatusResponse>(`/holds/${encodeURIComponent(holdId)}`);
  },

  async confirmHold(holdId: string, studentId?: string): Promise<ConfirmHoldResponse> {
    const id = studentId || activeStudentId || '';
    return request<ConfirmHoldResponse>(`/holds/${encodeURIComponent(holdId)}/confirm`, {
      method: 'POST',
      headers: {
        'X-Student-Id': id
      }
    });
  },

  async cancelHold(holdId: string, studentId?: string): Promise<void> {
    const id = studentId || activeStudentId || '';
    return request<void>(`/holds/${encodeURIComponent(holdId)}`, {
      method: 'DELETE',
      headers: {
        'X-Student-Id': id
      }
    });
  },

  async getMyHolds(studentId?: string): Promise<StudentHoldItem[]> {
    const id = studentId || activeStudentId || '';
    return request<StudentHoldItem[]>('/me/holds', {
      headers: {
        'X-Student-Id': id
      }
    });
  }
};

export { ApiClientError };
