import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { RegisterModel } from '../models/register.model';
import { LoginModel } from '../models/login.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = 'https://localhost:7051/api/Auth';

  constructor(private http: HttpClient){}

  public register(model: RegisterModel): Observable<any>{
    return this.http.post(`${this.apiUrl}/register`, model);
  }

  public login(model: LoginModel): Observable<any>{
    return this.http.post(`${this.apiUrl}/login`, model);
  }
}
