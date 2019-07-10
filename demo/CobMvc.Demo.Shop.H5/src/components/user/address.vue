<template>
    <ol class="">
        <li class="text-left" v-for="item in items" :key="item.id">
            <label>
                <input type="radio" v-model="selectedItem" :value="item">
                <span class="ml-2">{{item.province}}</span>
                <span class="ml-2">{{item.city}}</span>
            </label>
        </li>
    </ol>
</template>

<script>
export default {
    // name:'AddressList'
    props:{
        items:Array
    },
    data(){
        return {
            selectedItem:null
        }
    },
    methods:{
        getSelectedAddress(){
            return this.selectedItem;
        }
    }
}
</script>
