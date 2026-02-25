import {IClassService} from "@/injection/interfaces";
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

  public async createClass(
    name: string,
    students?: { number: string; firstName: string; lastName: string }[]
  ): Promise<ClassItem> {
    const body: Record<string, unknown> = { name };
    if (students && students.length > 0) body.students = students;
    const response = await this._httpClient.post<ClassItem>(
      `${import.meta.env.VITE_API_BASE_URL}/classes`,
      body,
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
    const response = await this
      ._httpClient
      .get<AxiosResponse<ExamItem[]>>(`${import.meta.env.VITE_API_BASE_URL}/classes/${classId}/exams`)
      .catch(function (error: AxiosError): AxiosResponse<ExamItem[]> {
        return error.response as AxiosResponse<ExamItem[]>
      })
    return response.data as ExamItem[]
  }

  public async createExam(classId: string, name: string): Promise<ExamItem> {
    const response = await this
      ._httpClient
      .post<any, AxiosResponse<ExamItem>>(
        `${import.meta.env.VITE_API_BASE_URL}/classes/${classId}/exams`,
        { classId, name },
        this.headersWithJsonContentType())
      .catch(function (error: AxiosError): AxiosResponse<ExamItem> {
        return error.response as AxiosResponse<ExamItem>
      })
    return response.data as ExamItem
  }

  public async deleteExam(examId: string): Promise<void> {
    await this
      ._httpClient
      .delete(`${import.meta.env.VITE_API_BASE_URL}/exams/${examId}`)
      .catch(function (error: AxiosError) {
        return error.response
      })
  }
}
