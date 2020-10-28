import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormControl, FormGroup, Validators} from '@angular/forms';
import { NewsService } from '../../shared/services/news.service';
import { TidingView, TidingTargetView, EntranceView } from '../../shared/models/news.model';
import { RequestFilter } from '../../shared/models/requests/request-filter';
import {HostService} from '../../shared/services/host.service';
//import { AngularEditorConfig } from '@kolkov/angular-editor';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AngularEditorConfig } from '../../global_components/angular-editor/config';
import { ObjectService } from '../../shared/services/objects.service';
import { PlacementService } from '../../shared/services/placements.service';
import {CompanyService} from '../../shared/services/companies.service';
import { PagerModel } from '../../shared/models/pager.model';

import { IDropdownSettings } from '../../global_components/ng-multiselect-dropdown';
import {Convertdate} from '../../global_components/convertdate';

@Component({
  selector: 'app-news',
  templateUrl: './news.component.html',
  styleUrls: ['./news.component.css']
})
export class NewsComponent implements OnInit {

  pager: PagerModel = new PagerModel();
  convertdate: Convertdate = new Convertdate();
  dropdownSettings: IDropdownSettings = {};
  ShowFilter:boolean;
  showAll:boolean = true;

  Filter: RequestFilter = new RequestFilter();
  NewsForm : FormGroup;
  readonly:boolean;
  sendbutton:boolean=false;
  errorframe:number=0;

  newseditor:boolean=false;
  newsviewer:boolean=false;
  formname:string="news";
  IsErrorForm:boolean=false;

  allnews:any;
  selectedtidingid:number;
  companies = [];
  selectedcompany:string;
  selectedcompanyid:number;
  objects = [];
  selectedobjectid:number;
  //entrances = [];
  placements = [];
  //numberOfEntrances:number;
  tidingtargets = [];
  
  TidingMessage:SafeHtml;
  TidingName:string;

  currentpage:number = 1;

  constructor(
    private companyService: CompanyService,
    private objSrv: ObjectService,
    private placementSrv: PlacementService,
    private newsService: NewsService,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer,
    private hostService: HostService
  ) { }

  editorConfig: AngularEditorConfig = {
    editable: true,
      spellcheck: true,
      height: 'auto',
      minHeight: '0',
      maxHeight: 'auto',
      width: 'auto',
      minWidth: '0',
      translate: 'yes',
      enableToolbar: true,
      showToolbar: true,
      placeholder: 'Введите текст сообщения...',
      newssection: true,
      defaultParagraphSeparator: '',
      defaultFontName: '',
      defaultFontSize: '',
      fonts: [
        {class: 'arial', name: 'Arial'},
        {class: 'times-new-roman', name: 'Times New Roman'},
        {class: 'calibri', name: 'Calibri'},
        {class: 'comic-sans-ms', name: 'Comic Sans MS'}
      ],
      customClasses: [
      {
        name: 'quote',
        class: 'quote',
      },
      {
        name: 'redText',
        class: 'redText'
      },
      {
        name: 'titleText',
        class: 'titleText',
        tag: 'h1',
      },
    ],
    uploadUrl: 'v1/image',
    //uploadWithCredentials: false,
    sanitize: true,
    toolbarPosition: 'top',
    toolbarHiddenButtons: [
      ['customClasses'],
      ['insertImage'],
      ['insertVideo'],
      ['insertHorizontalRule'],
      ['removeFormat'],
      ['toggleEditorMode']
    ]
  };

  ngOnInit() {
    this.createform();
    this.cleanup();

    this.dropdownSettings = {
      singleSelection: false,
      idField: 'item_id',
      textField: 'item_text',
      selectAllText: "Выделить всё",
      unSelectAllText: "Отменить выделение",
      itemsShowLimit: 3,
      allowSearchFilter: this.ShowFilter
    };

    this.newsService.Ping().subscribe(response => {
      this.readonly = !response.isAdmin;
      //if(this.readonly) this.createdisabledform();

        var id = window.sessionStorage.tsgmanageentityselectedic;
        this.companyService.GetCompanyListForFilter().subscribe(res => {
          this.companies = res;

          if(id >0)this.setCompany(id);
          else if(this.companies.length>0) this.setCompany(this.companies[0].id);
      });
    })
  }
  setCompany(id:number) {
    this.allnews = null;
    this.selectedtidingid = null;
    this.objects = null;
    //this.entrances = null;
    this.placements = null;
    this.selectedobjectid = null;
    //this.numberOfEntrances = null;
    this.tidingtargets = null;
    this.TidingMessage = null;
    this.TidingName = null;

        var selected = this.companies.find(a=>a.id == id);
      if(''+selected=='undefined') selected = this.companies[0];
      this.selectedcompany = selected.name;
      this.selectedcompanyid = selected.id;
      window.sessionStorage.tsgmanageentityselectedic = selected.id;
      window.sessionStorage.tsgmanagerrequestcurrentpage = 1;
      window.sessionStorage.tsgmanagercustomercurrentpage = 1;
        this.cleanup();
      this.newseditor = false;

//console.log('Company:' + id);
    this.GetNews();
  }

  ChangePage(page) {
    this.newsviewer = false;
    this.newseditor = false;
    this.tidingtargets = [];
    this.currentpage = page;
    this.GetNews();
  }

  GetNews(){
    this.Filter.CompanyId = this.selectedcompanyid;
    this.objSrv.GetObjectListWithFilter(this.Filter).subscribe(res => {
      this.objects = res;

      this.newsService.Dispatcherpage(this.selectedcompanyid, this.currentpage, true, true).subscribe(response => {
//console.log(response);
        this.allnews = response.collection;
        this.pager = response.pager;
        this.allnews.forEach(n => {
          n.createDate =  new Date(this.convertdate.fromGMT0(n.createDate)).toLocaleString('ru-RU');
        })
      })
    });
  }

  createform() {
    this.NewsForm = this.fb.group({
      Object: new FormControl('', []),
      Placement: new FormControl('', []),
      //Entrance: new FormControl('', []),
      TidingName: new FormControl('', []),
      TidingSubject: new FormControl('', []),
    });
  }

  //createdisabledform() {
  //  this.NewsForm = this.fb.group({
  //    Object: new FormControl({value: '', disabled: true}, []),
  //    Placement: new FormControl({value: '', disabled: true}, []),
  //    Entrance: new FormControl({value: '', disabled: true}, []),
  //    TidingName: new FormControl({value: '', disabled: true}, []),
  //    TidingSubject: new FormControl({value: '', disabled: true}, []),
  //  });
  //}

  cleanup() {
    this.NewsForm.get('Object').setValue(null);
    //this.NewsForm.get('Placement').setValue(null);
    //this.NewsForm.get('Entrance').setValue(null);
  }
  escape() {
    this.cleanup();
    this.newsviewer = false;
    this.newseditor = false;
    this.tidingtargets = [];
}
  add() {
    this.cleanup();
    this.newseditor = true;
    this.newsviewer = false;

    this.tidingtargets = new Array();
    //this.entrances = null;
    this.placements = null;

    this.selectedtidingid = 0;
    this.NewsForm.get('TidingName').setValue(null);
    this.NewsForm.get('TidingSubject').setValue(null);
    if(this.objects.length >0) {
      this.NewsForm.get('Object').setValue(this.objects[0].id);

//console.log('Object:' + this.objects[0].id);

      this.getObjectPlacements(null);
    }
  }
  edit(editModel: TidingView) {
    this.cleanup();
    this.newsviewer = true;
    this.newseditor = false;

    this.NewsForm.get('TidingName').setValue(editModel.name);
    this.NewsForm.get('TidingSubject').setValue(editModel.subject);
    
    this.TidingName = editModel.name;
    this.TidingMessage = this.sanitizer.bypassSecurityTrustHtml(editModel.subject);

    this.tidingtargets = new Array();

    //this.entrances = null;
    this.placements = null;

    //this.editorConfig.uploadUrl = this.hostService.hostReplaceToHttps() + 'api/Faq/addimage?id=' + id;
    this.selectedtidingid = editModel.id;
    window.sessionStorage.tsgmanagersavednewsid = editModel.id;

    if(this.objects.length >0) {
      this.NewsForm.get('Object').setValue(this.objects[0].id);

//console.log('Object:' + this.objects[0].id);

      this.getObjectPlacements(editModel);
    }
  }
  getObjectPlacements(editModel: TidingView) {
    this.selectedobjectid = this.NewsForm.get('Object').value;
    if(!this.selectedobjectid) return;
    if(this.selectedobjectid == 0) return;
//console.log(this.selectedobjectid);

    //this.numberOfEntrances = 0;
    //for(var i=0; i<this.objects.length; i++){
    //  if(this.objects[i].id == this.selectedobjectid){
    //    this.numberOfEntrances = this.objects[i].numberOfEntrances;
    //  }
    //}
    //if(this.numberOfEntrances == 0) return;

//console.log('Кол-во подъездов:' + this.numberOfEntrances);

    //this.entrances = new Array;
    //for(var i=0; i<this.numberOfEntrances; i++){
    //  var ent = new EntranceView();
    //  ent.id = i+1;
    //  this.entrances.push(ent);
    //}
    //this.NewsForm.get('Entrance').setValue(0);

    this.placementSrv.FindPlacement(this.selectedobjectid).subscribe(res => {
      this.placements = res;
      for(var i=0; i<this.placements.length;i++){
        var pl = this.placements[i];
        pl.item_id = pl.id;
        pl.item_text = 'Ул.'  + pl.street + ' д.' + pl.buildingNumber + ' кв.' + pl.placementNumber;
      }
      this.NewsForm.get('Placement').setValue(0);

      if(editModel && editModel.tidingtargets)
      {
        for(var z=0; z<editModel.tidingtargets.length;z++){
          var m = editModel.tidingtargets[z];
//console.log(m);
          var target = new TidingTargetView();
          target.tidingId = m.tidingId;
          target.companyId = m.companyId;
          target.objectId = m.objectId
          target.podezd = m.entrance;
          target.placementId = m.placementId;
          target.title = m.title;
          this.tidingtargets.push(target);
        }
      }
    });
  }

  onItemSelect(item: any) {
    //console.log('onItemSelect', item);
    //console.log('form model', this.NewsForm.get('Placement').value);
  }
  onItemDeSelect(item: any) {
    //console.log('onItem DeSelect', item);
    //console.log('form model', this.NewsForm.get('Placement').value);
  }

  onSelectAll(items: any) {
    //console.log('onSelectAll', items);
    //console.log('form model', this.NewsForm.get('Placement').value);
  }

  onDropDownClose() {
    //console.log('dropdown closed');
  }

  toogleShowAll() {
    this.showAll = !this.showAll;
    this.dropdownSettings = Object.assign({}, this.dropdownSettings, {
      enableCheckAll: this.showAll
    });
  }
  toogleShowFilter() {
    this.ShowFilter = !this.ShowFilter;
    this.dropdownSettings = Object.assign({}, this.dropdownSettings, {
      allowSearchFilter: this.ShowFilter
    });
  }

  //getEntrancePlacements() {
  //  var selectedentrance = this.NewsForm.get('Entrance').value;
  //  if(!selectedentrance) return;
  //  if(selectedentrance == 0) return;
    
  //  this.placementSrv.FindPlacementByEntrance(this.selectedcompanyid, this.selectedobjectid, selectedentrance).subscribe(res => {
  //    this.placements = res;
  //    this.NewsForm.get('Placement').setValue(0);
  //  });
  //}

  getPlacement(){
    //this.NewsForm.get('Entrance').setValue(0);
  }
  targetalreadyexists(placementId){
    if(placementId == 0 ) return false;
    if(!this.tidingtargets) return false;
    if(this.tidingtargets.length == 0) return false;

    for(var i=0; i<this.tidingtargets.length; i++){
      var target = this.tidingtargets[i];
      if(target.placementId == placementId) return true;
    }
}
  addtarget() {
    //var target = new TidingTargetView();
    //target.tidingId = this.selectedtidingid;
    //target.companyId = this.selectedcompanyid;

    this.errorframe = 0;

    var objectId = this.NewsForm.get('Object').value;

    //var entrance = this.NewsForm.get('Entrance').value;
    var selectedDropDownList = this.NewsForm.get('Placement').value;

    if(selectedDropDownList) {
      for(var i=0; i<selectedDropDownList.length; i++){
        var selectedDropDown = selectedDropDownList[i];
        if(this.targetalreadyexists(selectedDropDown.item_id)) continue;
        var target = this.createtarget(this.selectedtidingid, this.selectedcompanyid, objectId, null, selectedDropDown.item_id);
        this.tidingtargets.push(target);
      }
    }
    else {
      for(var i=0; i<this.placements.length; i++){
        var placement = this.placements[i];
        if(this.targetalreadyexists(placement.id)) continue;
        var target = this.createtarget(this.selectedtidingid, this.selectedcompanyid, objectId, null, placement.id);
        this.tidingtargets.push(target);
      }
    }
  }
  createtarget(tidingId, companyId, objectId, entrance, placementId):TidingTargetView{
    var target = new TidingTargetView();
    target.tidingId = tidingId;
    target.companyId = companyId;

    if(objectId != 0) target.objectId = objectId;
    //if(entrance != 0) target.podezd = entrance;
    if(placementId != 0) target.placementId = placementId;

    target.title = '';
    if(objectId != 0) {
      for(var i=0; i<this.objects.length; i++){
        if(this.objects[i].id == target.objectId){
          target.title += this.objects[i].address;
        }
      }
    }
    if(placementId != 0) {
      for(var i=0; i<this.placements.length; i++){
        if(this.placements[i].id == target.placementId){
          target.title += ', кв.' + this.placements[i].placementNumber;
        }
      }
    }
    //else if(entrance != 0) {
    //  for(var i=0; i<this.entrances.length; i++){
    //    if(this.entrances[i].id == target.podezd){
    //      target.title += ', подъезд №: ' + this.entrances[i].id;
    //    }
    //  }
    //}
    return target;
  }
  deletetarget(itemindex){
    this.tidingtargets.splice(itemindex, 1);
  }

  update() {
    if(!this.NewsForm.get('TidingName').value){
      alert('Введите тему сообщения');
      this.errorframe = 2;
      return;
    }
    if(!this.NewsForm.get('TidingSubject').value){
      alert('Введите сообщение');
      this.errorframe = 3;
      return;
    }
    var objectId = this.NewsForm.get('Object').value;
    var selectedDropDownList = this.NewsForm.get('Placement').value;
    if(selectedDropDownList && selectedDropDownList.length >0) {
      for(var i=0; i<selectedDropDownList.length; i++){
        var selectedDropDown = selectedDropDownList[i];
        if(this.targetalreadyexists(selectedDropDown.item_id)) continue;
        var target = this.createtarget(this.selectedtidingid, this.selectedcompanyid, objectId, null, selectedDropDown.item_id);
        this.tidingtargets.push(target);
      }
    }
    if(this.tidingtargets.length==0){
      alert('Добавьте адресата');
      this.errorframe = 1;
      return;
    }
    this.errorframe = 0;

    this.cleanup();
      this.newsviewer = false;
      this.newseditor = false;

      var tiding = new TidingView();
      tiding.companyId = this.selectedcompanyid;
      tiding.id = this.selectedtidingid;
      tiding.name = this.NewsForm.get('TidingName').value;
      tiding.subject = this.NewsForm.get('TidingSubject').value;
      tiding.tidingtargets = this.tidingtargets;

      this.newsService.Update(tiding).subscribe(response => {
        this.cleanup();
        this.newseditor = false;
        this.newsviewer = false;
        this.allnews = response;
        this.tidingtargets = [];
      });
  }
  viewcancel() {
    this.cleanup();
    this.newsviewer = false;
  }

  bin(model:TidingView) {
//console.log(model);
    var tiding = new TidingView();
    tiding.id = model.id;
    tiding.companyId = this.selectedcompanyid; 
    this.newsService.Bin(tiding).subscribe(response => {
      this.cleanup();
      this.newseditor = false;
      this.newsviewer = false;
      this.allnews = response;
      this.tidingtargets = [];
    })
  }

  HasError(control: string): boolean {
    if (this.IsErrorForm
      && !this.NewsForm.get(control).valid
      && !this.NewsForm.get(control).dirty) {
      return true;
    } else if (!this.IsErrorForm) {
      if (!this.NewsForm.get(control).valid && this.NewsForm.get(control).touched)  { return true; }
    }
    return false;
  }

  showsectiontools(id:number) {

  }

}
