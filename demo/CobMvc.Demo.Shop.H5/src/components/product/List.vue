<template>
    <div class="product-list">
        <transition-group name="product-item" >
            <ProductItem v-for="item in items" :key="item.id" :model="item" :showCheckbox="showCheckbox" ref="item" v-on:product:item:selected="selectProduct"></ProductItem>
        </transition-group>
    </div>
</template>

<script>
import ProductItem from './Item.vue'

export default {
    // name:'ProductList',
    components:{
        ProductItem
    },
    props:{
        items:Array,
        showCheckbox:Boolean
    },
    mounted(){

    },
    methods:{
        selectProduct(isSelected, item){
            // console.log(isSelected, item);
            this.$emit('product:item:selected', isSelected, item);
        }
    }
}
</script>

<style lang="" scoped>
    
</style>
