import { NgClass } from '@angular/common';
import { Component , ChangeDetectorRef} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LoginModel } from '../../models/login.model';
import { AuthService } from '../../services/auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [NgClass, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  public loginData: LoginModel = { email: '', password: '' };
  public message: string = '';
  public isError: boolean = false;
  public isLoading: boolean = false;

  constructor(private authService: AuthService, private router: Router, private cdr: ChangeDetectorRef) { }

  public onLogin(): void {
    this.message = '';
    this.isLoading = true;
    this.cdr.detectChanges();

    this.authService.login(this.loginData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.isError = false;
        this.message = 'Logged in succesfully!';
        this.cdr.detectChanges();

        localStorage.setItem('jwtToken', response.token);

        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.isError = true;
        this.message = 'Email or password incorrect!';
        this.cdr.detectChanges();
        console.error(err);
      }
    });
  }
}
