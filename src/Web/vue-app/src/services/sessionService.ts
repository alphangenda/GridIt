import {ISessionService} from "@/injection/interfaces";
import {injectable} from "inversify";
import {ApiService} from "./apiService";
import {AxiosError, AxiosResponse} from "axios";
import {SessionItem} from "@/types/entities";

@injectable()
export class SessionService extends ApiService implements ISessionService {
  public async getAllSessions(): Promise<SessionItem[]> {
    try {
      const response = await this._httpClient.get<SessionItem[]>(
        `${import.meta.env.VITE_API_BASE_URL}/sessions`
      );
      return Array.isArray(response.data) ? response.data : [];
    } catch {
      return [];
    }
  }

  public async getSession(sessionId: string): Promise<SessionItem> {
    const response = await this._httpClient.get<SessionItem>(
      `${import.meta.env.VITE_API_BASE_URL}/sessions/${sessionId}`
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to get session");
    }
    return response.data;
  }

  public async createSession(name: string, classIds?: string[]): Promise<SessionItem> {
    const response = await this._httpClient.post<SessionItem>(
      `${import.meta.env.VITE_API_BASE_URL}/sessions`,
      { name, classIds: classIds || [] },
      this.headersWithJsonContentType()
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to create session");
    }
    return response.data;
  }

  public async updateSession(sessionId: string, name: string, classIds?: string[]): Promise<SessionItem> {
    const response = await this._httpClient.put<SessionItem>(
      `${import.meta.env.VITE_API_BASE_URL}/sessions/${sessionId}`,
      { id: sessionId, name, classIds: classIds || [] },
      this.headersWithJsonContentType()
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to update session");
    }
    return response.data;
  }

  public async deleteSession(sessionId: string): Promise<void> {
    await this
      ._httpClient
      .delete(`${import.meta.env.VITE_API_BASE_URL}/sessions/${sessionId}`)
      .catch(function (error: AxiosError) {
        return error.response
      })
  }
}
