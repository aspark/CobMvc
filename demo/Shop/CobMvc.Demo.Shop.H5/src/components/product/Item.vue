<template>
    <dl class="product-item">
        <dt class="text-left">
            <label>            
                <input type="checkbox" v-model="isChecked" v-if="showCheckbox">
                {{model.name}}
            </label>
        </dt>
        <dd class="text-left text-muted ml-3">{{model.desc}}</dd>
    </dl>
</template>

<script>
export default {
    // name:'ProductItem',
    components:{},
    props:['model', 'showCheckbox'],
    data(){
        return {
            isChecked:false
        };
    },
    mounted(){
        
    },
    watch: {
        isChecked:function(val){
            this.$emit('product:item:selected', val, this.model)
        }
    }
}
</script>

<style lang="" scoped>
    
</style>
