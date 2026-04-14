import { injectable } from "inversify";
import { AxiosError, AxiosResponse } from "axios";
import { ApiService } from "@/services/apiService";
import { IProgramService } from "@/injection/interfaces";

type ProgramItem = { id: string; name: string };

@injectable()
export class ProgramService extends ApiService implements IProgramService {
  public async getAllPrograms(): Promise<ProgramItem[]> {
    const response = await this._httpClient
      .get<any, AxiosResponse<ProgramItem[]>>(`${import.meta.env.VITE_API_BASE_URL}/programs`)
      .catch((error: AxiosError): AxiosResponse<ProgramItem[]> => error.response as AxiosResponse<ProgramItem[]>);

    return Array.isArray(response?.data) ? response.data : [];
  }

  public async createProgram(name: string): Promise<ProgramItem> {
    const response = await this._httpClient
      .post<any, AxiosResponse<ProgramItem>>(
        `${import.meta.env.VITE_API_BASE_URL}/programs`,
        { name },
        this.headersWithJsonContentType()
      )
      .catch((error: AxiosError): AxiosResponse<ProgramItem> => error.response as AxiosResponse<ProgramItem>);

    if (!response?.data?.id)
      throw new Error("Failed to create program.");

    return response.data;
  }

  public async deleteProgram(programId: string): Promise<void> {
    await this._httpClient
      .delete(`${import.meta.env.VITE_API_BASE_URL}/programs/${programId}`)
      .catch((error: AxiosError) => error.response);
  }
}
