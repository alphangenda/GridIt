import {IGridService} from "@/injection/interfaces";
import {injectable} from "inversify";
import {ApiService} from "./apiService";

export interface GridItem {
  id: string;
  classId: string;
  name: string;
  courseCode: string;
  sessionName: string;
  isPublic: boolean;
  isOwner: boolean;
  creatorEmail: string;
  createdAt: string;
  groupNames: string[];
}

@injectable()
export class GridService extends ApiService implements IGridService {
  public async getAllGrids(): Promise<GridItem[]> {
    try {
      const response = await this._httpClient.get<GridItem[]>(
        `${import.meta.env.VITE_API_BASE_URL}/grids`
      );
      return Array.isArray(response.data) ? response.data : [];
    } catch {
      return [];
    }
  }

  public async toggleVisibility(examId: string, isPublic: boolean): Promise<boolean> {
    try {
      const response = await this._httpClient.patch(
        `${import.meta.env.VITE_API_BASE_URL}/grids/${examId}/visibility`,
        { examId, isPublic },
        this.headersWithJsonContentType()
      );
      return response.status >= 200 && response.status < 300;
    } catch {
      return false;
    }
  }
}
