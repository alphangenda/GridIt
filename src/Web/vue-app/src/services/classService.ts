import {IClassService, IDuplicateClassRequest, IDuplicationSource} from "@/injection/interfaces";
import {injectable} from "inversify";
import {ApiService} from "./apiService";
import {AxiosError, AxiosResponse} from "axios";
import {ClassItem, ExamItem} from "@/types/entities";

@injectable()
export class ClassService extends ApiService implements IClassService {
  public async getAllClasses(): Promise<ClassItem[]> {
    try {
      const response = await this._httpClient.get<ClassItem[]>(
        `${import.meta.env.VITE_API_BASE_URL}/classes`
      );
      return Array.isArray(response.data) ? response.data : [];
    } catch {
      return [];
    }
  }

  public async createClass(name: string, skillIds: string[], programId?: string): Promise<ClassItem> {
    const response = await this._httpClient.post<ClassItem>(
      `${import.meta.env.VITE_API_BASE_URL}/classes`,
      { name, skillIds, programId: programId ?? null },
      this.headersWithJsonContentType()
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to create class");
    }
    return response.data;
  }

  public async deleteClass(classId: string): Promise<void> {
    await this
      ._httpClient
      .delete(`${import.meta.env.VITE_API_BASE_URL}/classes/${classId}`)
      .catch(function (error: AxiosError) {
        return error.response
      })
  }

  public async getExamsByClass(classId: string): Promise<ExamItem[]> {
    try {
      const response = await this._httpClient.get<ExamItem[]>(
        `${import.meta.env.VITE_API_BASE_URL}/classes/${classId}/exams`
      );
      return Array.isArray(response.data) ? response.data : [];
    } catch {
      return [];
    }
  }

  public async createExam(classId: string, name: string): Promise<ExamItem> {
    const response = await this._httpClient.post<ExamItem>(
      `${import.meta.env.VITE_API_BASE_URL}/classes/${classId}/exams`,
      { classId, name },
      this.headersWithJsonContentType()
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to create exam");
    }
    return response.data;
  }

  public async deleteExam(examId: string): Promise<void> {
    await this
      ._httpClient
      .delete(`${import.meta.env.VITE_API_BASE_URL}/exams/${examId}`)
      .catch(function (error: AxiosError) {
        return error.response
      })
  }

  public async getDuplicationSources(): Promise<IDuplicationSource[]> {
    try {
      const response = await this._httpClient.get<IDuplicationSource[]>(
        `${import.meta.env.VITE_API_BASE_URL}/classes/duplication-sources`
      );
      return Array.isArray(response.data) ? response.data : [];
    } catch {
      return [];
    }
  }

  public async duplicateClass(request: IDuplicateClassRequest): Promise<ClassItem> {
    const response = await this._httpClient.post<ClassItem>(
      `${import.meta.env.VITE_API_BASE_URL}/classes/duplicate`,
      request,
      this.headersWithJsonContentType()
    );
    if (response.status < 200 || response.status >= 300 || !response.data) {
      throw new Error("Failed to duplicate class");
    }
    return response.data;
  }
}
