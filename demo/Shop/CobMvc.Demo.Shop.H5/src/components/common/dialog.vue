<template>
<div class="modal fade" :id="modelID" tabindex="-1" role="dialog">
  <div class="modal-dialog " role="document">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">{{title}}</h5>
        <button type="button" class="close" data-dismiss="modal" aria-label="Close">
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div class="modal-body">
        <slot/>
      </div>
      <div class="modal-footer" v-if="showFooter">
        <button type="button" class="btn btn-primary" @click="onConfirm&&onConfirm()">确定</button>
        <button type="button" class="btn btn-secondary" data-dismiss="modal" @click="onCancel&&onCancel()">取消</button>
      </div>
    </div>
  </div>
</div>
</template>

<script>
export default {
    props:{
      id:String,
      title:String,
      showFooter:Boolean,
      onConfirm:Function,
      onCancel:Function
    },
    data(){
      return {
        modelID:''
      };
    },
    mounted(){
      this.modelID = this.id||('dlg-' + (+new Date()));
    }
}
</script>