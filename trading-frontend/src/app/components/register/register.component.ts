import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { RegisterModel } from '../../models/register.model';
import { RouterLink } from '@angular/router';
import { TmplAstSwitchExhaustiveCheck } from '@angular/compiler';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [NgClass, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  public registerData: RegisterModel = {
    username: '',
    email: '',
    password: ''
  }

  public message: string = '';
  public isError: boolean = false;
  public isLoading: boolean = false;

  constructor(private authService: AuthService, private cdr: ChangeDetectorRef) { }

  public onRegister(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    this.authService.register(this.registerData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.isError = false;
        this.message = `Account created! You have $${response.initialBalance}.`;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.isError = true;
        // this.message = "Error when creating account. Password should contain capital letters, lowercase letters, numbers and symbols!";

        if(err.error && Array.isArray(err.error) && err.error.length > 0)
        {
          const firstError = err.error[0];

          this.message = firstError.description;
        }

        this.cdr.detectChanges();
        console.error(err);
      }
    });
  }
}
