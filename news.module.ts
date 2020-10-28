import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NewsComponent } from './news.component';
import { Routes, RouterModule } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule} from '@angular/common/http';
import { AngularEditorModule } from '../../global_components/angular-editor/angular-editor.module';
import { NgMultiSelectDropDownModule } from '../../global_components/ng-multiselect-dropdown';
import { PagerModule } from '../../global_components/pager/pager.module';

const routes: Routes = [
  {
    path: '',
    component: NewsComponent,
  }
];

@NgModule({
  declarations: [
    NewsComponent
  ],
  exports: [
    NewsComponent,
    RouterModule
  ],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ReactiveFormsModule,
    AngularEditorModule,
    PagerModule,
    NgMultiSelectDropDownModule
  ],
  providers: [
    HttpClientModule,
    AngularEditorModule
  ]
})
export class NewsModule { }
